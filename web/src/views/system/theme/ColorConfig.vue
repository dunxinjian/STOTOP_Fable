<script setup lang="ts">
import type { ThemeConfig } from '@/stores/theme'

defineProps<{
  editConfig: ThemeConfig
  previewTheme: any
}>()

const colorItems = [
  { key: 'colorPrimary', label: '主色', desc: '品牌主色调，用于按钮、链接等' },
  { key: 'colorSuccess', label: '成功色', desc: '成功状态、正向操作反馈' },
  { key: 'colorWarning', label: '警告色', desc: '警告状态、需要注意的操作' },
  { key: 'colorError', label: '错误色', desc: '错误状态、危险操作反馈' },
  { key: 'colorInfo', label: '信息色', desc: '一般信息提示、辅助说明' },
]
</script>

<template>
  <div class="config-section">
    <div class="section-header">
      <h3 class="section-title">色彩配置</h3>
      <p class="section-desc">自定义系统的主题色彩方案，影响按钮、标签、提示等组件的颜色</p>
    </div>

    <div class="config-items">
      <div v-for="item in colorItems" :key="item.key" class="config-row">
        <div class="config-label-area">
          <span class="config-label">{{ item.label }}</span>
          <span class="config-sublabel">{{ item.desc }}</span>
        </div>
        <div class="config-control color-control">
          <input type="color" v-model="(editConfig as any)[item.key]" class="color-input" />
          <a-input
            v-model:value="(editConfig as any)[item.key]"
            size="small"
            style="width: 100px"
            :maxlength="7"
          />
        </div>
      </div>
    </div>

    <a-divider style="margin: 24px 0 20px" />

    <div class="preview-area">
      <div class="preview-label">预览</div>
      <a-config-provider :theme="previewTheme">
        <div class="preview-content">
          <div class="preview-row">
            <a-space wrap>
              <a-button type="primary">主要按钮</a-button>
              <a-button>默认按钮</a-button>
              <a-button type="dashed">虚线按钮</a-button>
              <a-button type="text">文本按钮</a-button>
              <a-button type="link">链接按钮</a-button>
              <a-button danger type="primary">危险按钮</a-button>
            </a-space>
          </div>
          <div class="preview-row">
            <a-space>
              <a-tag color="processing">处理中</a-tag>
              <a-tag color="success">成功</a-tag>
              <a-tag color="warning">警告</a-tag>
              <a-tag color="error">错误</a-tag>
              <a-tag color="default">默认</a-tag>
            </a-space>
          </div>
          <div class="preview-row">
            <a-space direction="vertical" style="width: 100%">
              <a-alert message="信息提示" type="info" show-icon />
              <a-alert message="成功提示" type="success" show-icon />
              <a-alert message="警告提示" type="warning" show-icon />
              <a-alert message="错误提示" type="error" show-icon />
            </a-space>
          </div>
        </div>
      </a-config-provider>
    </div>
  </div>
</template>

<style scoped>
.config-section {
  /* shared styles from parent */
}

.section-header {
  margin-bottom: 24px;
}

.section-title {
  font-size: 16px;
  font-weight: 600;
  color: rgba(0, 0, 0, 0.88);
  margin: 0 0 4px;
}

.section-desc {
  font-size: 14px;
  color: rgba(0, 0, 0, 0.45);
  margin: 0;
}

.config-items {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.config-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.config-label-area {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.config-label {
  font-size: 14px;
  color: rgba(0, 0, 0, 0.88);
  font-weight: 500;
}

.config-sublabel {
  font-size: 12px;
  color: rgba(0, 0, 0, 0.45);
}

.config-control {
  display: flex;
  align-items: center;
}

.color-control {
  gap: 8px;
}

.color-input {
  width: 32px;
  height: 32px;
  padding: 2px;
  border: 1px solid #d9d9d9;
  border-radius: 6px;
  cursor: pointer;
  background: none;
}

.color-input::-webkit-color-swatch-wrapper {
  padding: 2px;
}

.color-input::-webkit-color-swatch {
  border: none;
  border-radius: 4px;
}

.preview-area {
  /* shared */
}

.preview-label {
  font-size: 14px;
  font-weight: 500;
  color: rgba(0, 0, 0, 0.65);
  margin-bottom: 12px;
}

.preview-content {
  background: #fafafa;
  border-radius: 6px;
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.preview-row {
  /* individual row */
}
</style>
